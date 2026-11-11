using System;
using System.Collections.Generic;
using UnityEngine;

// Identificadores dos ícones existentes; não dependem da ordem de compra.
public static class UpgradePurchases
{
    public const int CookingClock = 0;
    public const int Shoes = 1;
    public const int Backpack = 2;
    public const int Wristwatch = 3;
    public const int Wallet = 4;
    public const int Television = 11;
    public const int Magnet = 12;

    private static readonly HashSet<int> purchased = new HashSet<int>();
    public static event Action Changed;

    public static bool Has(int index) => purchased.Contains(index);

    // Chamado pela loja somente após a confirmação e o pagamento.
    public static void RegisterPurchase(int index)
    {
        if (index >= 0 && purchased.Add(index))
            Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        purchased.Clear();
        Changed = null;
    }
}
