using System;
using System.Collections.Generic;

public interface ILLMClient
{
    // Il metodo standard per chattare
    void SendChat(List<OllamaMessage> history, Action<string> onComplete);
}