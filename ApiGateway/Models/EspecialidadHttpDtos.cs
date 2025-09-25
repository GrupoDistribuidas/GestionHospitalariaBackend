namespace ApiGateway.Models;

public record EspecialidadCreateRequest(string nombre);
public record EspecialidadUpdateRequest(string nombre);
public record EspecialidadResponse(int id, string nombre);
