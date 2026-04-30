1. presigned urls
   - Use presigned url flow for uploads instead of uploading through the API, to avoid tying up API resources and hitting timeouts on large files. This also allows for better progress reporting on the frontend.
