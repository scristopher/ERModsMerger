using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Threading;

namespace ERModsMerger.Core.Utility;

/// <summary>
///     These RSA functions are copy-pasted straight from BinderTool. Thank you Atvaark!
/// </summary>
static class CryptoUtil {
    /// <summary>
    ///     Decrypts a file with a provided decryption key.
    /// </summary>
    /// <param name="filePath">An encrypted file</param>
    /// <param name="key">The RSA key in PEM format</param>
    /// <exception cref="ArgumentNullException">When the argument filePath is null</exception>
    /// <exception cref="ArgumentNullException">When the argument keyPath is null</exception>
    /// <returns>A memory stream with the decrypted file</returns>
    public static MemoryStream DecryptRsa(string filePath, string key, CancellationToken cancellationToken) {
        if (filePath == null) {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (key == null) {
            throw new ArgumentNullException(nameof(key));
        }

        AsymmetricKeyParameter keyParameter = getKeyOrDefault(key) ?? throw new InvalidOperationException();
        RsaEngine engine = new();
        engine.Init(false, keyParameter);

        MemoryStream outputStream = new();
        using (FileStream inputStream = File.OpenRead(filePath)) {

            int inputBlockSize = engine.GetInputBlockSize();
            int outputBlockSize = engine.GetOutputBlockSize();
            byte[] inputBlock = new byte[inputBlockSize];
            while (inputStream.Read(inputBlock, 0, inputBlock.Length) > 0) {
                cancellationToken.ThrowIfCancellationRequested();
                ;
                byte[] outputBlock = engine.ProcessBlock(inputBlock, 0, inputBlockSize);

                int requiredPadding = outputBlockSize - outputBlock.Length;
                if (requiredPadding > 0) {
                    byte[] paddedOutputBlock = new byte[outputBlockSize];
                    outputBlock.CopyTo(paddedOutputBlock, requiredPadding);
                    outputBlock = paddedOutputBlock;
                }

                outputStream.Write(outputBlock, 0, outputBlock.Length);
            }
        }

        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    private static AsymmetricKeyParameter? getKeyOrDefault(string key) {
        try {
            PemReader pemReader = new(new StringReader(key));
            return (AsymmetricKeyParameter)pemReader.ReadObject();
        }
        catch {
            return null;
        }
    }
}
