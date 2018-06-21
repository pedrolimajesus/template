///funtion for to validate layouts selected in the views:
/// /ContentManagerSchedulePlan/EditPlacement   and /ContentManagerSchedulePlan/Placements

function validationField() {
    var bandValidationFormat = false;
    bandValidationFormat = (validationFormatValueStartTime("#input-start-time", "#validationStartTime") && validationFormatValue("#duration", "#validationDurationTime") && validationLayout());
    return bandValidationFormat;
}

function validationLayout() {
    var result = false;
    var counter = 0;

    $("#table1").find(".content-td").each(function() {
        counter++;
    });

    $("#table1").find(".content-td1").each(function() {
        counter++;
    });

    result = counter > 0;

    if (result)
        $("#span_error_layout").css("display", "none");
    else
        $("#span_error_layout").css("display", "inline");

    return result;
}

function validationFormatValue(value, span) {
    var input = "#duration-select";
    var bandFormat = false;
    var objvalue = $(value).val();
    if (objvalue.length == 1 || objvalue.length == 2) {
        var num = parseInt(objvalue);
        if (num > 0 && num < 60)
            bandFormat = true;
        else
            bandFormat = false;
    } else {
        bandFormat = false;
    }
    if (!bandFormat) {
        $(input).addClass("input-validation-error");
        $(input).css("background", "none repeat scroll 0 0 #FEF1EC");
        $(span).text("Invalid Time Format.");
        $(span).addClass("field-validation-error");
        $(span).css("margin-top", "4px");
    }
    return bandFormat;
}

function validationFormatValueStartTime(value, span) {
    var input = "#input-start-time";
    var evaluation = false;
    var objvalue = $(value).val();
    if (objvalue.length == 8) {
        var regExPattern = /^([0-1][0-9]|2[0-3]):([0-5][0-9])(?::([0-5][0-9]))?$/;
        if (objvalue.toString().match(regExPattern))
            evaluation = true;
        else
            evaluation = false;
    }
    if (!evaluation) {
        $(input).addClass("input-validation-error");
        $(input).css("background", "none repeat scroll 0 0 #FEF1EC");
        $(span).text("Invalid Time Format.");
        $(span).addClass("field-validation-error");
        $(span).css("margin-top", "4px");
    }
    return evaluation;
}

function AddButtonsDeleteTimeLines() {
    $("a.link_hidden").each(function() {
        var idOne = $(this).attr('id');
        var dayOne = $(this).attr('day');
        var tooltip = $(this).attr('title');
        $(this).parent().parent().children().first().append("<a class='link-delete' title='delete timeline' onclick=\"javascript:DeleteTimeline('" + idOne + "','" + dayOne + "');\">&nbsp;</a>");
        $(this).parent().parent().attr("title", tooltip);
    });
}


function reduceText(obj, text, quantity) {
    var dottedSpace = "...";
    var newText = text;
    while (obj.height() > 30) {
        newText = newText.substr(0, newText.length - quantity);
        obj.text(newText);
    }
    newText = newText.substr(0, newText.length - quantity);
    obj.text(newText + dottedSpace);
}

function GetTextSize(text) {
    $("#footer-content .label_hidden").remove();
    return $("#footer-content").append("<label class='label_hidden' style='display:none;white-space: nowrap;'>" + text + "</label>").width();
}

/// 30px is aproximately the height of only one row of text, more height than 30px indicates that the text is 2 or more lines.
/// For to use correctly this function, the table must to follow the net standar: Must have a div inside the cell(td), and must have a label inside the div. 
function FormatLabelByHeight(rowTable) {

    $(rowTable).find("td").each(function() {
        var dottedSpace = "...";
        var widthTd = $(this).find("div").css("width");
        if (widthTd != null) {
            widthTd = widthTd.replace("px", "");
        }

        var labelHeight = $(this).find("div label").height();
        if ((widthTd != null || widthTd != "") && widthTd > 25) {
            var numberCaract = (widthTd * 13) / 100;
            var labelText = $(this).find("div label").text();
            labelText = labelText.trim();
            if (labelText != null || labelText != "") {
                if (labelText.length > numberCaract) {
                    var subLabelText = labelText.substr(0, numberCaract);
                    $(this).find("div label").text(subLabelText + dottedSpace);
                    if (labelHeight > 30) {
                        var newSize = subLabelText.length;
                        if (subLabelText.length > 50)
                            newSize = newSize / 2;
                        var textCutted = subLabelText.substr(0, newSize)
                        reduceTextByHeight($(this).find("div label"), textCutted, 2);
                    }
                }
            }
        }
    });
}


function reduceTextByHeight(obj, text, quantity) {
    var dottedSpace = "...";
    var newText = text;
    while (obj.height() > 30) {
        newText = newText.substr(0, newText.length - quantity).trim();
        if (newText.length > 30)
            newText = newText.substr(0, newText.length - quantity * 2).trim();
        obj.text(newText);
    }
    newText = newText.substr(0, newText.length - quantity).trim();
    obj.text(newText + dottedSpace);
}

/// size is the exact numbers of characters that can fill almost exactly the component width
/// this size must be reduced in 3 caracters for be reemplazed by three dots
function reduceTextBySize(obj, size) {
    var content = obj.text();
    if (obj.text().length > size) {
        obj.text(content.substr(0, size - 3) + "...");
    }
}

/* Timezone cookie */
function checkTimezoneCookie() {
    var tz = getCookie("timezone");
    if (tz != "") {
        window.console && window.console.log(tz); //log timezone
    } else {
        var tzObject = jstz.determine(); // Determines the time zone of the browser client
        tz = tzObject.name() + "|0";
        if (tz != "" && tz != null) {
            setCookie("timezone", tz, 0);
        }
    }
}

function deleteTimezoneCookie() {
    deleteCookie("timezone");
}

/* cookies */
function setCookie(cname, cvalue, exdays) {

    var expires = '';
    if (exdays != 0) {
        var d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        expires = "expires=" + d.toGMTString();
    }

    document.cookie = cname + "=" + cvalue + "; " + expires;
}

function getCookie(cname) {
    var name = cname + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i].trim();
        if (c.indexOf(name) == 0) return c.substring(name.length, c.length);
    }
    return "";
}

function deleteCookie(cname) {
    document.cookie = cname + "=; expires=" + new Date(1, 1, 1970).toGMTString();
}

function UserIsAutenticated() {
    var location1 = window.location.href.toString();
    var urlLogin = loginUrl + location1;
    $.ajax({
        url: verificationUrl + location1,
        type: 'GET',
        dataType: "json",
        success: function(data) {
            if (!data) {
                window.location.href = urlLogin;
            }
        }
    });
}

function onKeyDownEventHandler(event) {
    // Allow: backspace, delete, tab, escape, and enter
    if (event.keyCode == 46 || event.keyCode == 8 || event.keyCode == 9 || event.keyCode == 27 || event.keyCode == 13 ||
        // Allow: Ctrl+A
        (event.keyCode == 65 && event.ctrlKey === true) ||
        // Allow: home, end, left, right
        (event.keyCode >= 35 && event.keyCode <= 39)) {
        // let it happen, don't do anything
        return;
    } else {
        // Ensure that it is a number and stop the keypress
        if (event.shiftKey || (event.keyCode < 48 || event.keyCode > 57) && (event.keyCode < 96 || event.keyCode > 105)) {
            event.preventDefault();
        }
    }
}