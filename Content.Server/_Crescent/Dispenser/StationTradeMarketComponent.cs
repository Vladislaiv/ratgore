namespace Content.Server.Crescent.Dispenser;

[RegisterComponent]
public sealed partial class StationTradeMarketComponent : Component
{
    [DataField]
    public Dictionary<string, float> SalesAccumulator = new();

    /// <summary>
    /// На сколько цена снизится в процентах при продаже
    /// </summary>
    [DataField]
    public float PriceDropPerSale = 0.02f;

    /// <summary>
    /// Минимальная цена товара
    /// </summary>
    [DataField]
    public float MinMultiplier = 0.3f;

    /// <summary>
    /// За сколько одна еденица товара откатится в секундах
    /// </summary>
    [DataField]
    public float RecoveryRatePerSecond = 1f / 60f;
}