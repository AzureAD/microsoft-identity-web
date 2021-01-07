// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// An implementation of <see cref="ILoginErrorAccessor"/> that uses <see cref="ITempDataDictionary"/> to track error messages.
    /// </summary>
    internal class TempDataLoginErrorAccessor : ILoginErrorAccessor
    {
        private const string Name = "MicrosoftIdentityError";

        private readonly ITempDataDictionaryFactory _factory;
        private readonly IHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TempDataLoginErrorAccessor(ITempDataDictionaryFactory factory, IHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsEnabled => _env.IsDevelopment();

        public string? Message
        {
            get
            {
                if (IsEnabled)
                {
                    var d = GetDictionary();

                    if (d.TryGetValue(Name, out var result) && result is string msg)
                    {
                        return msg;
                    }
                }

                return null;
            }
            set
            {
                if (IsEnabled)
                {
                    var d = GetDictionary();
                    d.Add(Name, value);
                    d.Save();
                }
            }
        }

        private ITempDataDictionary GetDictionary() => _factory.GetTempData(_httpContextAccessor.HttpContext);
    }
}
