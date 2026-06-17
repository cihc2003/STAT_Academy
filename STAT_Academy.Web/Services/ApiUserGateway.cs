namespace STAT_Academy.Web.Services;

public class ApiUserGateway
{
    private readonly HttpClient _httpClient;

    public ApiUserGateway(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ApiIsReachableAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("Usuario/activos");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
