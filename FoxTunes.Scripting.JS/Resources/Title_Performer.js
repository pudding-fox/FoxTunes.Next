(function () {
    var parts = [];
    if (tag.title) {
        parts.push(tag.title);
    }
    if (tag.performer && tag.performer != tag.artist) {
        parts.push(tag.performer);
    }
    if (parts.length) {
        return parts.join(" - ");
    }
    else {
        return filename(file);
    }
})()