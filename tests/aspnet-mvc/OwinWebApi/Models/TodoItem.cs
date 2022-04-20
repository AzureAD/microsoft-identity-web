// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace OwinWebApi.Models
{
    public class Todo
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Owner { get; set; }
    }
}
