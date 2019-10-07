const DARK = "dark";
const CONTRAST = "contrast";

// To set the theme to the page
function setTheme(theme) {
    let bodyElement = $("body")[0];
    switch (theme) {
        case DARK:
            bodyElement.className = "theme-dark";
            break;
        case CONTRAST:
            bodyElement.className = "theme-highContrast";
            break;
        default:
            bodyElement.className = "theme-default";
            break;
    }
}

// To get the query string key and values
function getQueryParameters() {
    let queryParams = {};
    location.search.substr(1).split("&").forEach(function (item) {
        let s = item.split("="),
            k = s[0],
            v = s[1] && decodeURIComponent(s[1]);
        queryParams[k] = v;
    });
    return queryParams;
}