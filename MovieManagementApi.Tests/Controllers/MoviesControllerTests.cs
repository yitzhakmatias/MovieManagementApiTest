using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MovieManagementApi.Controllers;
using MovieManagementApi.Core.Entities;
using MovieManagementApi.Core.Services;
using MovieManagementApi.Core.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MovieManagementApi.Core.Interfaces;

namespace MovieManagementApi.Tests.Controllers
{
    public class MoviesControllerTests
    {
        private readonly MoviesController _moviesController;
        private readonly Mock<IMovieRepository> _mockMovieRepo;
        private readonly Mock<IActorRepository> _mockActorRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IOptions<AppSettings>> _mockOptions;

        public MoviesControllerTests()
        {
            // Mock the dependencies
            _mockMovieRepo = new Mock<IMovieRepository>();
            _mockActorRepo = new Mock<IActorRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockOptions = new Mock<IOptions<AppSettings>>();

            // Set up the mock for AppSettings (if you have API_SECRET_KEY in it)
            _mockOptions.Setup(o => o.Value).Returns(new AppSettings { API_SECRET_KEY = "123456" });

            // Initialize the controller with the mocked dependencies
            var movieService = new MovieService(_mockMovieRepo.Object);
            var actorService = new ActorService(_mockActorRepo.Object);
            _moviesController =
                new MoviesController(movieService, actorService, _mockMapper.Object, _mockOptions.Object);
        }

        [Fact]
        public async void TestGetMovies_ReturnsMovies()
        {
            var movies = new List<Movie>
            {
                new Movie
                {
                    Id = 1, Title = "Inception", Description = "Sci-Fi Thriller",
                    ReleaseDate = new DateTime(2010, 7, 16)
                }
            };

            _mockMovieRepo.Setup(m => m.GetAllMoviesAsync()).ReturnsAsync(movies);

            // Set up Mapper mock
            var movieDtos = new List<MovieDto>
            {
                new MovieDto
                {
                    Id = 1, Title = "Inception", Description = "Sci-Fi Thriller",
                    ReleaseDate = new DateTime(2010, 7, 16)
                }
            };
            _mockMapper.Setup(m => m.Map<List<MovieDto>>(It.IsAny<List<Movie>>())).Returns(movieDtos);

            // Act: Call the GetMovies method on the controller
            var result = await _moviesController.GetMovies();

            // Assert: Check if the result is of type OkObjectResult and contains the correct movie data
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var movieDtosResult = okResult.Value as List<MovieDto>;

            Assert.NotNull(movieDtosResult);
            Assert.Single(movieDtosResult);
            Assert.Equal("Inception", movieDtosResult[0].Title);
        }

        [Fact]
        public async Task GetMovies_ReturnsOkResult_WhenMoviesExist()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie
                {
                    Id = 1, Title = "Inception", Description = "Sci-Fi", ReleaseDate = new System.DateTime(2010, 7, 16)
                }
            };
            var movieDtos = new List<MovieDto>
            {
                new MovieDto
                {
                    Id = 1, Title = "Inception", Description = "Sci-Fi", ReleaseDate = new System.DateTime(2010, 7, 16)
                }
            };

            _mockMovieRepo.Setup(repo => repo.GetAllMoviesAsync()).ReturnsAsync(movies);
            _mockMapper.Setup(mapper => mapper.Map<List<MovieDto>>(It.IsAny<List<Movie>>())).Returns(movieDtos);

            // Act
            var result = await _moviesController.GetMovies();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<MovieDto>>(okResult.Value);
            Assert.Single(returnValue); // Assert that only one movie is returned
        }

        [Fact]
        public async Task GetMovies_ReturnsNotFound_WhenNoMoviesExist()
        {
            // Arrange
            _mockMovieRepo.Setup(repo => repo.GetAllMoviesAsync()).ReturnsAsync(new List<Movie>());
            _mockMapper.Setup(mapper => mapper.Map<List<MovieDto>>(It.IsAny<List<Movie>>()))
                .Returns(new List<MovieDto>());

            // Act
            var result = await _moviesController.GetMovies();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No movies found", notFoundResult.Value); // Ensure "No movies found" is returned
        }

        [Fact]
        public async Task CreateMovie_ReturnsUnauthorized_WhenApiKeyIsInvalid()
        {
            // Arrange
            var movie = new Movie
            {
                Id = 1, Title = "Inception", Description = "Sci-Fi Thriller",
                ReleaseDate = new System.DateTime(2010, 7, 16)
            };
            var invalidApiKey = "invalid_api_key"; // Invalid key

            // Act
            var result = await _moviesController.CreateMovie(movie, invalidApiKey);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid API Key", unauthorizedResult.Value); // Ensure the message is correct
        }

        [Fact]
        public async Task CreateMovie_ReturnsCreatedAtAction_WhenMovieIsCreated()
        {
            // Arrange
            var movie = new Movie
            {
                Id = 1, Title = "Inception", Description = "Sci-Fi Thriller",
                ReleaseDate = new System.DateTime(2010, 7, 16)
            };
            var validApiKey = "123456"; // Correct API Key

            _mockMovieRepo.Setup(repo => repo.AddMovieAsync(movie)).Returns(Task.CompletedTask);

            // Act
            var result = await _moviesController.CreateMovie(movie, validApiKey);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("GetMovie", createdResult.ActionName); // Ensure the action is correct
            Assert.Equal(1, createdResult.RouteValues["id"]); // Ensure the id passed in the route values is correct
        }

        [Fact]
        public async Task CreateMovie_ReturnsBadRequest_WhenMovieIsNull()
        {
            // Arrange
            Movie movie = null; // Null movie
            var validApiKey = "123456"; // Correct API Key

            // Act
            var result = await _moviesController.CreateMovie(movie, validApiKey);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result); // Ensure BadRequest is returned
        }

        [Fact]
        public async Task GetMovie_ReturnsOkResult_WhenMovieExists()
        {
            // Arrange
            var movie = new Movie
                { Id = 1, Title = "Inception", Description = "Sci-Fi", ReleaseDate = new System.DateTime(2010, 7, 16) };
            var movieDto = new MovieDto
                { Id = 1, Title = "Inception", Description = "Sci-Fi", ReleaseDate = new System.DateTime(2010, 7, 16) };

            _mockMovieRepo.Setup(repo => repo.GetMovieByIdAsync(1)).ReturnsAsync(movie);
            _mockMapper.Setup(mapper => mapper.Map<MovieDto>(It.IsAny<Movie>())).Returns(movieDto);

            // Act
            var result = await _moviesController.GetMovie(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<MovieDto>(okResult.Value);
            Assert.Equal(1, returnValue.Id); // Assert the movie ID is correct
        }

        [Fact]
        public async Task GetMovie_ReturnsNotFound_WhenMovieDoesNotExist()
        {
            // Arrange
            _mockMovieRepo.Setup(repo => repo.GetMovieByIdAsync(2)).ReturnsAsync((Movie)null);

            // Act
            var result = await _moviesController.GetMovie(2);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Movie not found", notFoundResult.Value); // Ensure the "Movie not found" message is returned
        }
    }
}