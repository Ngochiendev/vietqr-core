namespace VietQR.Deeplink.Client;

/// <summary>
/// Lỗi phát sinh khi gọi dịch vụ VietQR Deeplink.
/// </summary>
public class VietQrDeeplinkException : Exception
{
    public VietQrDeeplinkException(string message) : base(message)
    {
    }

    public VietQrDeeplinkException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
