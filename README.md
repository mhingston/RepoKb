# RepoKb: Index and Search your Code Repository

RepoKb is a simple proof of concept that uses [Kernel Memory](https://microsoft.github.io/kernel-memory) to perform Retrieval Augmented Generation (RAG) on a git repository.

[Qdrant](https://qdrant.tech/) is used as the vector database to store/retrieve embeddings of the code repository.
[Azure OpenAI](https://azure.microsoft.com/en-us/products/cognitive-services/openai-service) is used to generate the embeddings and for chat completion.

## Prerequisites

- **.NET 8 SDK:** Make sure you have the .NET 8 SDK installed on your system.
- **Docker:** You need a Docker compatible engine installed to run the [Qdrant vector database](https://qdrant.tech/documentation/quickstart/).
- **User Secrets:** You need to set up your Azure OpenAI credentials and Qdrant connection string as user secrets.
  - `Azure.OpenAI.ApiKey`
  - `Qdrant.ConnectionString`

## Setup

1. Open a terminal in the project's root directory.
2. **Run Qdrant Docker Image:**
   ```bash
   docker run -d -p 6333:6333 -v qdrant_storage:/qdrant/storage qdrant/qdrant
   ```
   
   This will start the Qdrant container and mount the `qdrant_storage` directory on your host machine to `/qdrant/storage` inside the container.

## Usage

**1. Index the Repository:**

   ```bash
   dotnet run --mode index --path "path/to/your/repository"
   ```
- This will clear the Blob Storage container, index the specified repository, and upload the Qdrant data to Blob Storage.
- You will be prompted to stop the Qdrant container after the indexing is complete.

**2. Search the Repository:**

   ```bash
   dotnet run --mode search
   ```
- This will download the Qdrant data from Blob Storage (if it's not already present) and start a minimal API for searching.
- You will be prompted to start the Qdrant container after the download is complete.
- Navigate to http://localhost:5000/swagger in your browser to interact with the API.

**Search API:**

- **Endpoint:** `/search` (POST)
- **Request Body:**
  ```json
  {
    "query": "your search query",
    "limit": 10 // Optional, default is 10
  }
  ```
- **Response Body:**
  ```json
  {
    "results": [
      // List of search results (format depends on Kernel Memory implementation)
    ]
  }
  ```