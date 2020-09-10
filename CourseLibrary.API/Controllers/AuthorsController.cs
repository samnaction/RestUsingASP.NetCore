namespace CourseLibrary.API.Controllers
{
    using AutoMapper;
    using CourseLibrary.API.ActionConstraints;
    using CourseLibrary.API.Entities;
    using CourseLibrary.API.Helpers;
    using CourseLibrary.API.Models;
    using CourseLibrary.API.ResourceParameters;
    using CourseLibrary.API.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Net.Http.Headers;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthorsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;

        private readonly IMapper _mapper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyCheckerService _propertyCheckerService;

        public AuthorsController(ICourseLibraryRepository courseLibraryRepository, IMapper mapper,
            IPropertyMappingService propertyMappingService, IPropertyCheckerService propertyCheckerService)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _propertyMappingService = propertyMappingService ??
                throw new ArgumentNullException(nameof(propertyMappingService));
            _propertyCheckerService = propertyCheckerService ??
                throw new ArgumentNullException(nameof(propertyCheckerService));
        }

        [HttpGet(Name = "GetAuthors")]
        [HttpHead]
        public IActionResult GetAuthors(
            [FromQuery] AuthorsResourceParameters authorsResourceParameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);

            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

            var links = CreateLinksForAuthors(authorsResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

            var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).
                ShapeData(authorsResourceParameters.Fields);

            var shapedAuthorsWithLink = shapedAuthors.Select(author =>
            {
                var authorAsDictionary = author as IDictionary<string, object>;
                var authorLinks = CreateLinkForAuthor((Guid)authorAsDictionary["Id"], null);
                authorAsDictionary.Add("links", authorLinks);
                return authorAsDictionary;
            });

            var linkedCollectionResouce = new
            {
                value = shapedAuthorsWithLink,
                links
            };

            return Ok(linkedCollectionResouce);

        }

        [HttpGet("{authorId:guid}", Name = "GetAuthor")]
        [Produces("application/json",
            Const.HateoasJson,
            Const.AuthorFriendlyJson,
            Const.AuthorFriendlyHateoasJson,
            Const.AuthorFullJson,
            Const.AuthorFullHateoasJson)]
        public IActionResult GetAuthor(Guid authorId, string fields, [FromHeader(Name = "Accept")] string mediaType)
        {
            if(!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            {
                return BadRequest();
            }

            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);
            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var includeLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith("hateoas",
                StringComparison.InvariantCultureIgnoreCase);

            IEnumerable<LinkDto> links = new List<LinkDto>();

            if (includeLinks)
            {
                links = CreateLinkForAuthor(authorId, fields);
            }

            var primaryMediaType = includeLinks ? parsedMediaType.SubTypeWithoutSuffix.Substring(0,
                parsedMediaType.SubTypeWithoutSuffix.Length - 8) : parsedMediaType.SubTypeWithoutSuffix;


            if(primaryMediaType == Const.AuthorFull)
            {
                var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo).ShapeData(fields)
                    as IDictionary<string, object>;

                if (includeLinks)
                {
                    fullResourceToReturn.Add("links", links);
                }

                return Ok(fullResourceToReturn);
            }

            var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields)
                    as IDictionary<string, object>;

            if (includeLinks)
            {
                friendlyResourceToReturn.Add("links", links);
            }

            return Ok(friendlyResourceToReturn);


        }

        [HttpPost(Name = "CreateAuthorWithDateofDeath")]
        [RequestHeaderMatchesMediaType("Content-Type", Const.AuthorForCreationWithDateOfDeath)]
        [Consumes(Const.AuthorForCreationWithDateOfDeath)]
        public ActionResult<AuthorDto> CreateAuthorWithDateofDeath(AuthorForCreationWithDateOfDeathDto 
            authorForCreationDto)
        {
            var authorEntity = _mapper.Map<Entities.Author>(authorForCreationDto);
            _courseLibraryRepository.AddAuthor(authorEntity);
            _courseLibraryRepository.Save();

            var authorDto = _mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinkForAuthor(authorDto.Id, null);

            var linkedResourceToReturn = authorDto.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { authorId = linkedResourceToReturn["Id"] },
                linkedResourceToReturn);
        }


        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type","application/json",Const.AuthorForCreationJson)]
        [Consumes("application/json",Const.AuthorForCreationJson)]
        public ActionResult<AuthorDto> CreateAuthor(AuthorForCreationDto authorForCreationDto)
        {
            var authorEntity = _mapper.Map<Entities.Author>(authorForCreationDto);
            _courseLibraryRepository.AddAuthor(authorEntity);
            _courseLibraryRepository.Save();

            var authorDto = _mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinkForAuthor(authorDto.Id, null);

            var linkedResourceToReturn = authorDto.ShapeData(null) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { authorId = linkedResourceToReturn["Id"] },
                linkedResourceToReturn);
        }

        [HttpOptions]
        public IActionResult GetAuthorsOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }

        [HttpDelete("{authorId}", Name = "DeleteAuthor")]
        public ActionResult DeleteAuthor(Guid authorId)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            _courseLibraryRepository.DeleteAuthor(authorFromRepo);
            _courseLibraryRepository.Save();

            return NoContent();
        }

        private string CreateAuthorsResourcesUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType
            resourceUriType)
        {
            switch (resourceUriType)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize,
                            mainCategory = authorsResourceParameters.MainCategory,
                            searchQuery = authorsResourceParameters.SearchQuery
                        });

                case ResourceUriType.NextPage:
                    return Url.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize,
                            mainCategory = authorsResourceParameters.MainCategory,
                            searchQuery = authorsResourceParameters.SearchQuery
                        });

                case ResourceUriType.Current:
                default:
                    return Url.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize,
                            mainCategory = authorsResourceParameters.MainCategory,
                            searchQuery = authorsResourceParameters.SearchQuery
                        });
            }
        }

        private IEnumerable<LinkDto> CreateLinkForAuthor(Guid authorId, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(Url.Link("GetAuthor", new { authorId }), "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(Url.Link("GetAuthor", new { authorId, fields }), "self", "GET"));
            }

            links.Add(new LinkDto(Url.Link("DeleteAuthor", new { authorId }), "delete_author", "DELETE"));

            links.Add(new LinkDto(Url.Link("CreateCourseForAuthor", new { authorId }), "create_course_for_author",
                "POST"));

            links.Add(new LinkDto(Url.Link("GetCoursesForAuthor", new { authorId }), "courses",
                "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters, 
            bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();
            links.Add(new LinkDto(CreateAuthorsResourcesUri(authorsResourceParameters, ResourceUriType.Current),
                "self", "GET"));

            if(hasNext)
            {
                links.Add(new LinkDto(CreateAuthorsResourcesUri(authorsResourceParameters, ResourceUriType.NextPage),
                "nextPage", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateAuthorsResourcesUri(authorsResourceParameters, ResourceUriType.PreviousPage),
                "previousPage", "GET"));
            }

            return links;
        }
    }
}
