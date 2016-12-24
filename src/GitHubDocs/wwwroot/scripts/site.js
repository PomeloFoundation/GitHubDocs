var anchors = $('.nav-list a');
for (var i = 0; i < anchors.length; i++) {
    var nxt = $(anchors[i]).next();
    if (nxt.length > 0 && nxt[0].tagName.toUpperCase() == 'UL') {
        $(anchors[i]).prepend('<i class="fa fa-angle-right"></i>');
    } else {
        $(anchors[i]).addClass('non-arrow');
    }
    if ($(anchors[i]).attr('href').indexOf('http') < 0 && $(anchors[i]).attr('href').indexOf('//') < 0 && $(anchors[i]).attr('href').indexOf('javascript') < 0 && $(anchors[i]).attr('href').indexOf('#') < 0)
        $(anchors[i]).attr('href', '/' + $(anchors[i]).attr('href'));
    else if ($(anchors[i]).attr('href').indexOf('http') >= 0 && $(anchors[i]).attr('href').indexOf('//') >= 0)
        $(anchors[i]).attr('target', '_blank');
}

if (anchors.length > 1) {
    $(anchors[0]).children('i').remove();
}

var active = $('a[href="' + endpoint + '"]');
$('head').append('<title>' + active.text() + '</title>');
active.children('i').removeClass('fa-angle-right').addClass('fa-angle-down');
active.addClass('active');
var current = active;
while (current.length > 0) {
    current.parents('ul').show();
    current.prev().children('i').removeClass('fa-angle-right').addClass('fa-angle-down');
    current.next().show();
    current = current.parents('ul');
}
if (branch) {
    for (var i = 0; i < anchors.length; i++) {
        if ($(anchors[i]).attr('href').indexOf('http') < 0 && $(anchors[i]).attr('href').indexOf('//') < 0 && $(anchors[i]).attr('href').indexOf('javascript') < 0 && $(anchors[i]).attr('href').indexOf('#') < 0)
            $(anchors[i]).attr('href', '/' + branch + $(anchors[i]).attr('href'));
    }
}

var blocks = $('blockquote');
for (var i = 0; i < blocks.length; i++) {
    $(blocks[i]).html($(blocks[i]).html().replace(/\n/g, "</p>\n<p>"));
    if ($(blocks[i]).html().indexOf('[!TIP]') >= 0) {
        $(blocks[i]).html($(blocks[i]).html().replace('[!TIP]', '<strong>TIP</strong>'));
        $(blocks[i]).addClass('tip');
    }
    if ($(blocks[i]).html().indexOf('[!NOTE]') >= 0) {
        $(blocks[i]).html($(blocks[i]).html().replace('[!NOTE]', '<strong>NOTE</strong>'));
        $(blocks[i]).addClass('info');
    }
    if ($(blocks[i]).html().indexOf('[!WARNING]') >= 0) {
        $(blocks[i]).html($(blocks[i]).html().replace('[!WARNING]', '<strong>WARNING</strong>'));
        $(blocks[i]).addClass('warn');
    }
}

if ($('.doc-content h1').length > 0 && $('article#main').length == 0) {
    $($('.doc-content h1')[0]).after($('.content-contribution'));
    $('.content-contribution').after('<div class="clear"></div>');
    $('.content-contribution').show();
}

function Expand(anchor) {
    $(anchor).next().slideDown();
    $(anchor).children('i').removeClass('fa-angle-right').addClass('fa-angle-down');
}

function ShowBranches() {
    $('.nav-branch-list').toggle();
    var top = $('.nav-branch').offset().top;
    var height = $('.nav-branch-list').outerHeight();
    $('.nav-branch-list').css('top', top - height);
}

$(window).click(function (e) {
    if (!$(e.target).hasClass('nav-branch-list') && !$(e.target).hasClass('nav-branch') && $(e.target).parents('.nav-branch-list').length == 0 && $(e.target).parents('.nav-branch').length == 0) {
        $('.nav-branch-list').hide();
    }
});

$('.nav-show-button').click(function () {
    $('.nav').show();
    $('.nav-show-button').hide();
});

$('.nav-close-button').click(function () {
    $('.nav').hide();
    $('.nav-show-button').show();
});

Highlight();