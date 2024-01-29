window.BENCHMARK_DATA = {
  "lastUpdate": 1706493704647,
  "repoUrl": "https://github.com/AzureAD/microsoft-identity-web",
  "entries": {
    "TokenAcquisitionBenchmarks": [
      {
        "commit": {
          "author": {
            "email": "jeferrie@microsoft.com",
            "name": "jennyf19",
            "username": "jennyf19"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "edf100eb0f8a7f7fe3acba14093f458c055e6579",
          "message": "update env variables for benchmark (#2658)",
          "timestamp": "2024-01-28T17:55:22-08:00",
          "tree_id": "425da43913bea202432110239d18a7dbfe85d26d",
          "url": "https://github.com/AzureAD/microsoft-identity-web/commit/edf100eb0f8a7f7fe3acba14093f458c055e6579"
        },
        "date": 1706493703480,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.TokenAcquisitionBenchmark.CreateAuthorizationHeader",
            "value": 10277.643803563611,
            "unit": "ns",
            "range": "± 81.28328745160938"
          },
          {
            "name": "Benchmarks.TokenAcquisitionBenchmark.GetTokenAcquirer",
            "value": 10867.143365930628,
            "unit": "ns",
            "range": "± 56.27880623538526"
          }
        ]
      }
    ]
  }
}