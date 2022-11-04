// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// @see https://github.com/microsoft/BotBuilder-Samples/blob/main/samples/csharp_dotnetcore/51.teams-messaging-extensions-action/Bots/TeamsMessagingExtensionsActionBot.cs


namespace Stickers.Bot;

using System.Text.RegularExpressions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;
using Stickers.Entities;
using Stickers.Models;
using Stickers.Resources;

public partial class TeamsMessagingExtensionsBot : TeamsActivityHandler
{
    private static readonly Regex IMG_SRC_REGEX = new("<img[^>]+src=\"([^\"\\s]+)\"[^>]*>");
    private static readonly Regex IMG_ALT_REGEX = new("<img[^>]+alt=\"([^\"]+)\"[^>]*>");
    private static readonly Regex IMAGE_URL_REGEX = new("^http(s?):\\/\\/.*\\.(?:jpg|gif|png)$");

    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(
        ITurnContext<IInvokeActivity> turnContext,
        MessagingExtensionAction action,
        CancellationToken cancellationToken
    )
    {
        var command = action.CommandId;

        switch (command)
        {
            case "management":
                return this.ManageTaskModule(turnContext.Activity);
            case "collect":
                return await this.SaveCollection(turnContext.Activity);
            default:
                this.logger.LogError("unkwon command: {command}", command);
                throw new Exception("unkwon bot action command " + command);
        }
    }

    private async Task<MessagingExtensionActionResponse> SaveCollection(IInvokeActivity activity)
    {
        var value = (JObject)activity.Value;
        var payload = value?["messagePayload"]?.Value<JObject>();
        var body = payload?["body"]?.Value<JObject>();
        var userId = activity.From.AadObjectId;
        var content = body?["content"]?.ToString();
        var imgs = GetImages(content);
        List<Attachment>? attachments = payload?["attachments"]?.ToObject<List<Attachment>>();
        attachments?.ForEach(
            (attachment) =>
            {
                imgs.AddRange(GetImageFromAttachment(attachment));
            }
        );
        var hasImg = imgs.Count > 0;
        var entities = new List<Sticker>();
        foreach (var img in imgs)
        {
            entities.Add(
                new Sticker
                {
                    src = img.Src,
                    name = img.Alt,
                    id = Guid.NewGuid()
                }
            );
        }
        await this.stickerService.addUserStickers(new Guid(userId), entities);
        var locale = activity.GetLocale();
        JObject cardJson;
        if (hasImg)
        {
            // a workround for template can not handler josn array
            cardJson = GetAdaptiveCardJsonObject(
                new { Imgs = imgs, src = imgs[0].Src },
                "SaveCard.json"
            );
        }
        else
        {
            cardJson = GetAdaptiveCardJsonObject(
                new
                {
                    BlankText = LocalizationHelper.LookupString(
                        "collect_save_no_images_found",
                        locale
                    )
                },
                "BlankCard.json"
            );
        }

        var a = new Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = cardJson,
        };
        return new MessagingExtensionActionResponse()
        {
            Task = new TaskModuleContinueResponse
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = hasImg
                        ? LocalizationHelper.LookupString("collect_save_success", locale)
                        : LocalizationHelper.LookupString("collect_save_fail", locale),
                    Height = hasImg ? 300 : 60,
                    Width = 300,
                    Card = a,
                },
            },
        };
    }

    private static List<Img> GetImages(string? content)
    {
        List<Img> imgs = new List<Img>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return imgs;
        }
        var result = IMG_SRC_REGEX.Matches(content);
        foreach (Match match in result.Cast<Match>())
        {
            var alt = IMG_ALT_REGEX.Match(match.Groups[0].Value)?.Groups?[1].Value;
            imgs.Add(new Img { Src = GetWrapUrl(match.Groups[1].Value), Alt = alt });
        }
        return imgs;
    }

    private static List<Img> GetImageFromAttachment(Attachment attachment)
    {
        List<Img> imgs = new List<Img>();
        if (
            attachment.ContentType.StartsWith("image")
            || (attachment.ContentUrl != null && IMAGE_URL_REGEX.IsMatch(attachment.ContentUrl))
        )
        {
            imgs.Add(new Img { Src = GetWrapUrl(attachment.ContentUrl) });
        }
        if (
            attachment.ContentType == "application/vnd.microsoft.card.adaptive"
            || attachment.ContentType == "application/vnd.microsoft.card.hero"
        )
        {
            var originContent = attachment.Content;
            if (originContent == null)
            {
                return imgs;
            }

            var content =
                originContent is string
                    ? JObject.Parse((originContent as string)!)
                    : JObject.FromObject(originContent);
            var body = content?["body"]?.ToObject<List<JObject>>();
            if (body == null)
            {
                return imgs;
            }
            foreach (var item in body)
            {
                Img? img;
                if (item["type"]?.ToString() == "Container")
                {
                    // wrap with container
                    var card = item["items"]?.ToObject<List<JObject>>()?.First();
                    img = ParseImgFromImgCard(card);
                }
                else
                {
                    img = ParseImgFromImgCard(item);
                }
                if (img != null)
                {
                    imgs.Add(img);
                }
            }
        }

        return imgs;
    }

    private static Img? ParseImgFromImgCard(JObject? item)
    {
        if (item != null && item["type"]?.ToString() == "Image")
        {
            var url = GetWrapUrl(item["url"]?.ToString());
            return new Img
            {
                Src = url,
                Alt = item["alt"] == null ? item["altText"]?.ToString() : item["alt"]?.ToString()
            };
        }
        return null;
    }

    /// <summary>
    /// https://us-prod.asyncgw.teams.microsoft.com/urlp/v1/url/content?url=https%3a%2f%2fsticker.newfuture.cc%2fofficial-stickers%2f0001%2f03.png
    /// </summary>
    /// <returns></returns>
    private static string GetWrapUrl(string? url)
    {
        var split = url?.Split('?', 2);
        if (
            split != null
            && split.Length == 2
            && split[0].EndsWith("/url/content")
            && split[1].StartsWith("url=http")
        )
        {
            return Uri.UnescapeDataString(split[1][4..]);
        }
        return url ?? "";
    }

    /// <summary>
    /// configuration taskmodule
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    private MessagingExtensionActionResponse ManageTaskModule(IInvokeActivity activity)
    {
        var userId = activity.From.AadObjectId;
        var response = new MessagingExtensionActionResponse()
        {
            Task = new TaskModuleContinueResponse
            {
                Type = "continue",
                Value = new TaskModuleTaskInfo
                {
                    Title = LocalizationHelper.LookupString(
                        "upload_task_module_title",
                        activity.GetLocale()
                    ),
                    Url = this.GetConfigUrl(Guid.Parse(userId!)),
                    Width = "large",
                    Height = "large",
                }
            },
        };
        return response;
    }
}
