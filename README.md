# Basic distributed system project

The intent of this project is to investigate deploying and running a small distributed system using [Docker](https://www.docker.com/)/[Kubernetes](https://kubernetes.io/).

# Architecture

There are three services comprising the application: `WebClient`, `Worker` and `RabbitMQ`. `WebClient` and `Worker` do not communicate directly with each other. Instead, `RabbitMQ` will handle the communication via queues.

## WebClient

`WebClient` is the asp.net core web interface which allows users to interact with the system. The state of the system is currently in memory so multiple instances can be run, but they won't share state.

## Worker

When the `WebClient` requests work, that work is performed on the `Worker` application. Multiple instances of the worker can be run to scale out the work.

## RabbitMQ

[`RabbitMQ`](https://www.rabbitmq.com/) is the service being used to broker messages between the `WebClient(s)` and the `Worker(s)`.

## Queue Architecture

The entire application makes use of a single, direct exchange. There are two types of queues being used: the worker queue (singleton) and the reply queues (many).

### Worker queue

This is the queue which the `WebClients` will publish work requests to and the `Workers` will consume to pick up work.

### Reply queues

When the `WebClients` send messages, it will add a queue name to the `replyTo` field of the message properties. This allows the worker to send updates to the correct web client.

Each `WebClient` will have it's own reply queue.

### Queue and Exchange lifecycle

When either a `Worker` or a `WebClient` roles is started and begins to publish or consume messages, they will both ensure the exchange and main work queue exists before attempting to publish or consume messages. This is intended to account for potential startup time variance between the `WebClients` and the `Workers`.

The reply queue for the `WebClient` is managed solely by the `WebClient` itself. It will be deleted when the `WebClient` stops.

# Deployment

## Docker

### Build

The docker file will publish the dotnet project and build the container. The path for the build must be the `src` directory (this was the easiest way I found to build/publish when referencing multiple csproj projects):

Worker:

`docker build -t bds-worker -f src/Worker/Dockerfile ./src`

Web Client:

`docker build -t bds-webclient -f src/WebClient/Dockerfile ./src`

### Compose

Compose will build the `WebClient` and `Worker` images, so the entire process can be run in a single step:

`docker compose up`

## Kubernetes

### Prerequisites

* [RabbitMQ Operator](https://www.rabbitmq.com/kubernetes/operator/operator-overview.html)

### Deployment

Once the prerequisites are running on the cluster, the system can be deployed with `kubectl -k ./.k8s` which will apply the kustomize file in the directory. This will deploy all the required components. Note that the Web and Worker deployments might restart while the RabbitMQ operator deploys the RabbitMQ cluster.

# Telemetry

All telemetry goes via the [OpenTelemetry collecter](https://opentelemetry.io/docs/collector/). Run the telemetry stack with `docker compose -f .metrics/docker-compose.yml up -d`.

# TODO:
1. ~~Run services on docker~~
2. ~~Run services on kubernetes~~
3. Get kubernetes to autoscale based on worker queue depth (using KEDA?)
4. Add telemetry
