# parent image
FROM python:3.6-alpine

# OS installs
RUN apk add --no-cache \
    bash \
    build-base

COPY . /app

WORKDIR /app

RUN pip install -r /app/requirements.txt

ENTRYPOINT ["python"]
CMD ["server.py"]