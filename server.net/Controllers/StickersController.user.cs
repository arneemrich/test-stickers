using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stickers.Entities;
using Stickers.Models;
using Stickers.Service;
using System.Security.Claims;

namespace Stickers.Controllers;

[ApiController]
[Route("api/me/stickers")]
public class StickersController : ControllerBase
{


    private readonly ILogger<StickersController> _logger;
    private StickerStorage stickerStorage;
    private BlobService blobService;
    private IHttpContextAccessor httpContextAccessor = null;

    public StickersController(StickerStorage stickerStorage, BlobService blobService, ILogger<StickersController> logger, IHttpContextAccessor httpContextAccessor)
    {
        this.stickerStorage = stickerStorage;
        this.blobService = blobService;
        _logger = logger;
        this.httpContextAccessor = httpContextAccessor;
    }
    public Guid GetUserId(Guid oldId)
    {
        //var userId = this.httpContextAccessor.HttpContext?.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
        //return new Guid(userId);

        //should call hulin's function
        return oldId;
    }

    [HttpPost("commit")]
    public async Task<Sticker> Commit([FromQuery]Guid userId, [FromBody] PostStickerBlobRequest request)
    {
        userId = GetUserId(userId);
        string extendName = System.IO.Path.GetExtension(request.name);
        string src = await this.blobService.commitBlocks(userId, request.id, extendName, request.contentType);
        var newSticker = new Sticker()
        {
            src = src,
            name = Path.GetFileNameWithoutExtension(request.name) + extendName,
            id = Guid.Parse(request.id)
        };
        var list = await this.stickerStorage.addUserStickers(userId, new List<Sticker>() { newSticker });
        return list[0];

    }
    [HttpGet("/api/me/stickers")]
    public async Task<Page<Sticker>> Get(Guid userId)
    {
        userId = this.GetUserId(userId);
        var stickers = await this.stickerStorage.getUserStickers(userId);
        return new Page<Sticker>(stickers);
    }
    [HttpDelete("{id}")]
    public async Task<Result> Delete(string id, Guid userId)
    {
        userId = this.GetUserId(userId);
        var result = await this.stickerStorage.deleteUserSticker(userId, id);
        return new Result(result);
    }

    [HttpPatch("{id}")]
    public async Task<Result> UpdateSticker(string id, string userId, string name)
    {
        userId = this.GetUserId(userId);
        var result = await this.stickerStorage.updateStickerName(userId, id, name);
        return new Result(result);
    }

}
public class PostStickerBlobRequest
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? contentType { get; set; }
}
