package main

import (
	"encoding/json"
	"fmt"
	"net/http"
)

// Hello : sample "hello world" endpoint
func Hello(w http.ResponseWriter, req *http.Request) {
	payload := "hello world"
	b, err := json.Marshal(payload)
	if err != nil {
		fmt.Fprintf(w, err.Error())
		return
	}
	fmt.Fprintf(w, string(b))
}

func main() {
	prepend := "/api"

	// example api
	http.HandleFunc(prepend+"/hello", Hello)

	// serve
	http.ListenAndServe(":5000", nil)
}
