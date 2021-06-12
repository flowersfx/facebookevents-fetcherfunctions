# Facebook Events Fetcher Functions

An Azure timer function in C# to retrieve a specific subset of event fields for all upcoming events published on a specific Facebook Page and store them in blob storage as a flattened json file.

Deploy to Azure Functions and make sure the following application settings are present:

```FACEBOOK_PAGE_ACCESS_TOKEN```

A page access token generated and extended for Facebook Graph API.

```FACEBOOK_PAGE_ID```

The numerical page id of the page to pull events from, can be found by visiting the page and looking at the bottom of the About section.

```BLOB_STORAGE_CONNECTION_STRING```

The connection string to Azure storage.

```BLOB_STORAGE_CONTAINER_NAME```

The container name for a blob container in the Azure storage.

```BLOB_STORAGE_FILE_NAME```

The filename to store json results in, eg "events.json"

For local testing, add these application settings to a local.settings.json file in the project root.