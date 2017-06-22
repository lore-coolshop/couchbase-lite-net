﻿// 
// SslStreamFactory.cs
// 
// Author:
//     Jim Borden  <jim.borden@couchbase.com>
// 
// Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Couchbase.Lite.DI;

namespace Couchbase.Lite.Support
{
    internal sealed class SslStreamFactory : ISslStreamFactory
    {
        #region ISslStreamFactory

        public ISslStream Create(Stream inner)
        {
            return new SslStreamImpl(inner);
        }

        #endregion
    }

    internal sealed class SslStreamImpl : ISslStream
    {
        #region Variables

        private readonly SslStream _innerStream;

        #endregion

        #region Properties

        public bool AllowSelfSigned { get; set; }

        public X509Certificate2 PinnedServerCertificate { get; set; }

        #endregion

        #region Constructors

        public SslStreamImpl(Stream inner)
        {
            _innerStream = new SslStream(inner, false, ValidateServerCert);
        }

        #endregion

        #region Private Methods

        private bool ValidateServerCert(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (PinnedServerCertificate != null) {
                // Pinned certs take priority over everything
                return certificate.Equals(PinnedServerCertificate);
            }

            if (sslPolicyErrors == SslPolicyErrors.None) {
                return true;
            }

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors)) {
                if (chain?.ChainStatus != null) {
                    foreach (var status in chain.ChainStatus) {
                        if (certificate.Subject == certificate.Issuer &&
                            status.Status == X509ChainStatusFlags.UntrustedRoot) {
                            // Self-signed certificates with an untrusted root are potentially valid. 
                            continue;
                        }

                        if (status.Status != X509ChainStatusFlags.NoError) {
                            // If there are any other errors in the certificate chain, the certificate is invalid,
                            // so the method returns false.
                            return false;
                        }
                    }

                    // When processing reaches this line, the only errors in the certificate chain are 
                    // untrusted root errors for self-signed certificates. Go by the overall setting
                    return AllowSelfSigned;
                }
            }

            return false;
        }

        #endregion

        #region ISslStream

        public Stream AsStream()
        {
            return _innerStream;
        }

        public Task ConnectAsync(string targetHost, ushort targetPort, X509CertificateCollection clientCertificates,
            bool checkCertificateRevocation)
        {
            return _innerStream.AuthenticateAsClientAsync(targetHost, clientCertificates, SslProtocols.Tls12,
                checkCertificateRevocation);
        }

        #endregion
    }
}
