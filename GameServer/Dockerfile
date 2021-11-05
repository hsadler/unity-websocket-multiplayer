FROM python:3.10-alpine

RUN apk add --no-cache \
    bash \
    build-base

COPY . /gameserver

WORKDIR /gameserver

RUN pip install -r /gameserver/requirements.txt
