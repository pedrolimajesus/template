/// 30px is aproximately the height of only one row of text, more height than 30px indicates that the text is 2 or more lines.
/// For to use correctly this function, the table must to follow the net standar: Must have a div inside the cell(td), and must have a label inside the div. 
function FormatLabelByHeight(rowTable) {

    $(rowTable).find("td").each(function () {
        var dotted_space = "...";
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
                    $(this).find("div label").text(subLabelText + dotted_space);
                    if (labelHeight > 30) {
                        var new_size = subLabelText.length;
                        if (subLabelText.length > 50)
                            new_size = new_size / 2;
                        var text_cutted = subLabelText.substr(0, new_size)
                        reduceTextByHeight($(this).find("div label"), text_cutted, 2);
                    }
                }
            }
        }
    });
}


function reduceTextByHeight(obj, text, quantity) {
    var dotted_space = "...";
    var newText = text;
    while (obj.height() > 30) {
        newText = newText.substr(0, newText.length - quantity).trim();
        if (newText.length > 30)
            newText = newText.substr(0, newText.length - quantity * 2).trim();
        obj.text(newText);
    }
    newText = newText.substr(0, newText.length - quantity).trim();
    obj.text(newText + dotted_space);
}

/// size is the exact numbers of characters that can fill almost exactly the component width
/// this size must be reduced in 3 caracters for be reemplazed by three dots
function reduceTextBySize(obj, size) {
    var content = obj.text();
    if (obj.text().length > size) {
        obj.text(content.substr(0, size - 3) + "...");
    }
}
