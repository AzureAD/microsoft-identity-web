// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// An implementation of <see cref="ILoginErrorAccessor"/> that uses <see cref="ITempDataDictionary"/> to track error messages.
    /// </summary>
    internal class TempDataLoginErrorAccessor : ILoginErrorAccessor
    {
        private const string Name = "MicrosoftIdentityError";

        private readonly ITempDataDictionaryFactory _factory;

        private ITempDataDictionary? _tempData;

        public static ILoginErrorAccessor Create(
            ITempDataDictionaryFactory? factory,
            bool isDevelopment)
        {
            if (!isDevelopment || factory is null)
            {
                return new EmptyLoginErrorAccessor();
            }
            else
            {
                return new TempDataLoginErrorAccessor(factory);
            }
        }

        private TempDataLoginErrorAccessor(ITempDataDictionaryFactory factory)
        {
            _factory = factory;
        }

        public bool IsEnabled => true;

        public string? GetMessage(HttpContext context)
        {
            if (_tempData == null)
            {
                _tempData = _factory.GetTempData(context);
            }

            if (_tempData.TryGetValue(Name, out var result) && result is string msg)
            {
                return msg;
            }

            return null;
        }

        public void SetMessage(HttpContext context, string? message)
        {
            if (message != null)
            {
                _tempData = _factory.GetTempData(context);

                _tempData.Add(Name, message);
                _tempData.Save();
            }
        }

        private class EmptyLoginErrorAccessor : ILoginErrorAccessor
        {
            public bool IsEnabled => false;

            public string? GetMessage(HttpContext context)
                => null;

            public void SetMessage(HttpContext context, string? message)
            {
                // This is empty.
            }
        }
    }
}
