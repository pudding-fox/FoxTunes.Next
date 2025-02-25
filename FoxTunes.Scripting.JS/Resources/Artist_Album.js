(function () {
    var parts = [tag.artist || strings.general_noartist];
    if (tag.album) {
        parts.push(tag.album);
    }
    return parts.join(" - ");
})()