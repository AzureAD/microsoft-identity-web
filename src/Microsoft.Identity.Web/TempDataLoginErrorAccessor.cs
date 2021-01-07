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

        public TempDataLoginErrorAccessor(ITempDataDictionaryFactory factory, IHostEnvironment env)
        {
            _factory = factory;

            IsEnabled = env.IsDevelopment();
        }

        public bool IsEnabled { get; }

        public string? GetMessage(HttpContext context)
        {
            if (IsEnabled)
            {
                var tempData = _factory.GetTempData(context);

                if (tempData.TryGetValue(Name, out var result) && result is string msg)
                {
                    return msg;
                }
            }

            return null;
        }

        public void SetMessage(HttpContext context, string? message)
        {
            if (IsEnabled && message != null)
            {
                var tempData = _factory.GetTempData(context);

                tempData.Add(Name, message);
                tempData.Save();
            }
        }
    }
}
