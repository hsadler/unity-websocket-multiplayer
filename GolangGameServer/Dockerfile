FROM golang:1.17.3-alpine3.14

# copy over server files
COPY . /go/src/

# set working directory
WORKDIR /go/src/

# for hot reload
RUN go get github.com/githubnemo/CompileDaemon

