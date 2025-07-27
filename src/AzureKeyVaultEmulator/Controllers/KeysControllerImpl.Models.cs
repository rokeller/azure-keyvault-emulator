using System;
using System.Collections.Generic;
using System.Xml;

namespace AzureKeyVaultEmulator.Controllers;

partial class KeysControllerImpl
{
    private readonly record struct BackedUpKeyVersions(
        string BackupVersion,
        string Name,
        List<KeyBundle> Versions
    );
}

partial class KeyRotationPolicyAttributes
{
    internal TimeSpan? ExpireTimeSpan
    {
        get
        {
            if (null != ExpiryTime)
            {
                return XmlConvert.ToTimeSpan(ExpiryTime);
            }

            return null;
        }
    }
}
