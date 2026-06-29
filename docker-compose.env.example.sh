#!/usr/bin/env bash

export GITHUB_USERNAME=""
export GITHUB_PAT=""
export MAILTRAP_TOKEN=""
export MAILTRAP_WEBHOOK_SIGNING_SECRET=""
export MAILTRAP_FROM_ADDRESS=""
export MAILTRAP_FROM_NAME="BackendProjectTemplate"

docker compose up -d --build --force-recreate