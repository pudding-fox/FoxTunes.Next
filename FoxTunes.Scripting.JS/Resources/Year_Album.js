(function () {
    if (tag.album) {
        var parts = [];
        if (tag.year) {
            parts.push(tag.year);
        }
        parts.push(tag.album);
        return parts.join(" - ");
    }
    return strings.general_noalbum;
})()