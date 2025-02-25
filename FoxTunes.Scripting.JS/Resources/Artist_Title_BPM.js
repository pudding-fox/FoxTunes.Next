(function () {
    var parts = [];
    if (tag.performer) {
        parts.push(tag.performer);
    }
    else if (tag.artist) {
        parts.push(tag.artist);
    }
    else {
        parts.push(strings.general_noartist);
    }
    if (tag.title) {
        parts.push(tag.title);
    }
    else {
        parts.push(strings.general_notitle);
    }
    if (tag.beatsperminute) {
        parts.push("[" + tag.beatsperminute + "]");
    }
    else {
        parts.push("[" + strings.general_unknown + "]")
    }
    return parts.join(" - ");
})()