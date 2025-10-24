namespace FmuApiDomain.CentralServiceExchange.Interfaces;

public interface ICentralServerExchangeActions
{
    Task<bool> StartExchange();
}