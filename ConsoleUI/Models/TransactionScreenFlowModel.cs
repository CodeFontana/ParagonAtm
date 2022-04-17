﻿namespace ConsoleUI.Models;

public class TransactionScreenFlowModel
{
    public string Screen { get; set; }
    public int Timeout { get; set; }
    public int RefreshInterval { get; set; }
    public string ActionType { get; set; }
    public string ActionValue { get; set; }
}