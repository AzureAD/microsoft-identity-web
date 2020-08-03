using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    public class MicrosoftWebApiAuthenticationBuilder
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="services"> The services being configured.</param>
        /// <param name="jwtBearerAuthenticationScheme">Defaut scheme used for OpenIdConnect.</param>
        /// <param name="configureMicrosoftIdentityOptions">Action called to configure
        /// the <see cref="MicrosoftIdentityOptions"/>Microsoft identity options.</param>
        internal MicrosoftWebApiAuthenticationBuilder(
            IServiceCollection services,
            string jwtBearerAuthenticationScheme,
            Action<MicrosoftIdentityOptions> configureMicrosoftIdentityOptions)
        {
            Services = services;
            _jwtBearerAuthenticationScheme = jwtBearerAuthenticationScheme;
            _configureMicrosoftIdentityOptions = configureMicrosoftIdentityOptions;

            if (_configureMicrosoftIdentityOptions == null)
            {
                throw new ArgumentNullException(nameof(configureMicrosoftIdentityOptions));
            }
        }

        /// <summary>
        /// The services being configured.
        /// </summary>
        public virtual IServiceCollection Services { get; private set; }

        private Action<MicrosoftIdentityOptions> _configureMicrosoftIdentityOptions { get; set; }

        private string _jwtBearerAuthenticationScheme { get; set; }

        MicrosoftWebApiAuthenticationBuilder CallWebApi();
    }
}
