![.NET Core Build and Test](https://github.com/Cingulara/openrmf-msg-checklist/workflows/.NET%20Core%20Build%20and%20Test/badge.svg)

# openrmf-msg-checklist
> *As of version 1.8, this functionality is moved into the openrmf-api-read project to reduce the footprint and number of components.*

Messaging service to respond to internal API requests to receive artifact and checklist information using a NATS Request/Reply scenario.
* openrmf.checklist.read to get a single checklist
* openrmf.system.checklists.read to get all checklists for a given system

## Running the NATS docker images
* docker run --rm --name nats-main -p 4222:4222 -p 6222:6222 -p 8222:8222 nats:2.1.2-linux
* this is the default and lets you run a NATS server version 2.x (as of 12/2019)
* just runs in memory and no streaming (that is separate)

## What is required
* .NET Core 3.x
* running `dotnet add package NATS.Client` to add the package
* dotnet restore to pull in all required libraries
* The C# NATS client library available at https://github.com/nats-io/csharp-nats

## Making your local Docker image
* make build
* make latest

## creating the database user
* ~/mongodb/bin/mongo 'mongodb://root:myp2ssw0rd@localhost'
* use admin
* db.createUser({ user: "openrmf" , pwd: "openrmf1234!", roles: ["readWriteAnyDatabase"]});
* use openrmf
* db.createCollection("Artifacts");

## connecting to the database collection straight
~/mongodb/bin/mongo 'mongodb://openrmf:openrmf1234!@localhost/openrmf?authSource=admin'

## List out the Artifacts you have inserted/updated
db.Artifacts.find();