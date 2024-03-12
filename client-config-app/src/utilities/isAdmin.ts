export function IsAdmin(idToken: string) {
    try {
        // https://learn.microsoft.com/en-us/azure/active-directory/roles/permissions-reference#role-template-ids
        // keep it the same with the server side
        // https://github.com/NewFuture/my-stickers/blob/v2/server.net/Program.cs#L47

        const decode = parseJwt(idToken);
        return decode.scp === "Admin"
    } catch (e) {
        return false;
    }
}

function parseJwt(token: string) {
    var base64Url = token.split(".")[1];
    var base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    var jsonPayload = decodeURIComponent(
        window
            .atob(base64)
            .split("")
            .map(function (c) {
                return "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2);
            })
            .join(""),
    );

    return JSON.parse(jsonPayload);
}
