using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
    {
        var query = DB.PagedSearch<Item, Item>();

        SearchBySearchTerm(searchParams, query);
        ApplyOrderCriteria(searchParams, query);
        ApplyFilterCriteria(searchParams, query);
        SearchBySeller(searchParams, query);
        SearchByWinner(searchParams, query);
        SetPageNumberAndSize(searchParams, query);

        var result = await query.ExecuteAsync();

        return Ok(new
        {
            results = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount
        });
    }

    private static void SearchBySearchTerm(SearchParams searchParams, PagedSearch<Item, Item> query)
    {
        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
        }
    }

    private static void ApplyOrderCriteria(SearchParams searchParams, PagedSearch<Item, Item> query)
    {
        query = searchParams.OrderBy switch
        {
            "make" => query.Sort(x => x.Ascending(a => a.Make)),
            "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
            _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
        };
    }

    private static void ApplyFilterCriteria(SearchParams searchParams, PagedSearch<Item, Item> query)
    {
        query = searchParams.FilterBy switch
        {
            "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
            "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow),
            _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow),
        };
    }

    private static void SearchBySeller(SearchParams searchParams, PagedSearch<Item, Item> query)
    {
        if (!string.IsNullOrEmpty(searchParams.Seller))
        {
            query.Match(x => x.Seller == searchParams.Seller);
        }
    }

    private static void SearchByWinner(SearchParams searchParams, PagedSearch<Item, Item> query)
    {
        if (!string.IsNullOrEmpty(searchParams.Winner))
        {
            query.Match(x => x.Winner == searchParams.Winner);
        }
    }

    private static void SetPageNumberAndSize(SearchParams searchParams, PagedSearch<Item, Item> query)
    {
        query.PageNumber(searchParams.PageNumber);
        query.PageSize(searchParams.PageSize);
    }
}