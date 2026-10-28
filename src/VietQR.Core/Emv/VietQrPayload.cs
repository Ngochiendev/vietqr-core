namespace VietQR.Core;

/// <summary>
/// Tiện ích đóng gói chuỗi payload EMVCo VietQR chuẩn NAPAS247.
/// </summary>
public static class VietQrPayload
{
    public static string Create(
        BankBin bin,
        string accountNo,
        long? amount = null,
        string? description = null,
        VietQrServiceType serviceType = VietQrServiceType.Account)
    {
        var builder = new VietQr()
            .SetBeneficiaryOrganization(bin.Value, accountNo, serviceType);

        if (amount.HasValue && amount.Value > 0)
        {
            builder.SetTransactionAmount(amount.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.SetAdditionalData(purpose: description);
        }

        return builder.Build();
    }
}
