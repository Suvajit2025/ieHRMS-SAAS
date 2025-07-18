$.ajaxPrefilter(function (options, originalOptions, jqXHR) {
    const token = window.ACCESS_TOKEN;
    if (token) {
        jqXHR.setRequestHeader("Authorization", "Bearer " + token);
    } else {
        console.warn("No access token found – Authorization header not set.");
    }
});
