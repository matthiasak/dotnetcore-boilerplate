using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.Http;
using System;

[Route("/")]
public class HomeController : Controller
{
    private IRepository<Card> cards;
    private IRepository<CardList> lists;
    private IRepository<Board> boards;
    public HomeController(IRepository<Card> cards, IRepository<CardList> lists, IRepository<Board> boards){
        this.cards = cards;
        this.lists = lists;
        this.boards = boards;
    }

    [HttpGet("/{username?}")]
    [HttpGet("home/index/{username?}")]
    public IActionResult Root(string username = "you")
    {
        // Console.WriteLine(HttpContext);
        ViewData["Message"] = "Some extra info can be sent to the view";
        ViewData["Username"] = username;
        return View("Index"); // View(new Student) method takes an optional object as a "model", typically called a ViewModel
    }

    // [HttpGet("sql/cards")] // ?sql=....
    // public IActionResult SqlCards([FromQuery]string sql) => Ok(cards.FromSql(sql));

    // [HttpGet("sql/lists")] // ?sql=....
    // public IActionResult SqlLists([FromQuery]string sql) => Ok(lists.FromSql(sql));

    // [HttpGet("sql/boards")] // ?sql=....
    // public IActionResult SqlBoards([FromQuery]string sql) => Ok(boards.FromSql(sql));
}