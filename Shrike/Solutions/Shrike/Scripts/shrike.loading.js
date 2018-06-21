function submitFormPost(form, caller, waitMessage, imageUrl, screenId) {
    $(caller).prop("disabled", true);
    $(caller).addClass("button-disabled");

    // Maybe show a loading indicator...
    showLoadingScreen(caller, waitMessage, imageUrl, screenId);

    form.submit();
}

function showLoadingScreen(caller, waitMessage, imageUrl, screenId) {
    var screen = createOrUpdateLoadingScreen(waitMessage, imageUrl, screenId);
    screen.show();
    var panel = screen.siblings().last();
    panel.show();
}

function hideLoadingScreen(caller, screenId) {
    $(caller).prop("disabled", false);
    var screen = $("#" + screenId);
    screen.hide();
    var panel = screen.siblings().last();
    panel.hide();
}

function createOrUpdateLoadingScreen(waitMessage, imageUrl, screenId) {
    var screen = $("#" + screenId);
    console.log(waitMessage + "-" + imageUrl + "-" + screenId);

    if (screen.length == 0) {
        $("body").append("<div id='" + screenId + "' title='" + waitMessage + "' class='hide loading-panel'></div>" +
            "<div class='loading-center-panel hide'>" +
            "<img class='loading-image' alt='loading' src='" + imageUrl + "' />" +
            "<div class='loading-message'>" + waitMessage + "</div>" +
            "</div>");
        screen = $("#" + screenId);

        //console.log("screen created> " + screen.html() + "-" + panel.html());
    } else {
        var panel = screen.siblings().last();
        if (waitMessage != '') {
            screen.attr("title", waitMessage);
            panel.find(".loading-message").html(waitMessage);
        }

        if (imageUrl != '') {
            panel.find(".loading-image").attr("src", imageUrl);
        }
        //console.log("screen updated> " + screen.html() + "-" + panel.html());
    }

    return screen;
}
