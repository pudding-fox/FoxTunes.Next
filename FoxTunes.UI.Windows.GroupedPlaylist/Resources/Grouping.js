(function () {
    var parts = [];
    if (tag.iscompilation || tag.__ft_variousartists) {
        parts.push(strings.general_variousartists);
    }
    else if (tag.artist) {
        parts.push(tag.artist);
    }
    if (tag.year) {
        parts.push(tag.year);
    }
    if (tag.album) {
        parts.push(tag.album);
    }
    else {
        parts.push(directoryname(file));
    }
    return parts.join(" - ");
})()