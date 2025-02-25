(function () {
    var parts = [];
    if (tag.artist) {
        parts.push(tag.artist);
    }
    if (tag.album) {
        parts.push(tag.album);
    }
    return parts.join(" - ");
})()