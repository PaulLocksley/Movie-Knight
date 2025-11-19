var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Movie_Knight>("movie-knight")
    .WithExternalHttpEndpoints();

builder.Build().Run();

