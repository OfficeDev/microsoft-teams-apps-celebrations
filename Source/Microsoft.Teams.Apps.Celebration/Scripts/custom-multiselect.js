(function ($) {
    function updateSelectedText(selector, options) {
        // This function can be used to customize the display of multiple selections
    }

    function addCustomCheckbox(selector) {
        selector.nextAll(".btn-group:first").find(".multiselect-container .checkbox").append("<span class='checkmark'></span>");
    }

    function bindEvents(selector,options) {
        selector.closest(".multiselect-native-select").find(".multiselect.dropdown-toggle").click(function (event) {
            hideAllDropdowns();
            selector.nextAll(".slimScrollDiv:first").toggleClass("show").toggleClass("hide");
            $(this).closest(".multiselect-native-select").find(".multiselect-container").toggleClass("show");
            event.stopPropagation();
        });

        selector.closest(".multiselect-native-select").find(".multiselect-container [type = 'checkbox']").change(function () {
            updateSelectedText($(this), options);
        });
    }

    function hideAllDropdowns() {
       $(".dropdown-menu.show").removeClass("show").parent(".slimScrollDiv").removeClass("show").addClass("hide");

        // hide any open date picker
       $(".datepicker.dropdown-menu").css({ "display": "none" });
    }

    function singleSelectElementClick(selector) {
        var liElement = selector.closest(".multiselect-native-select").find(".multiselect-container li");
         liElement.click(function () {
             liElement.removeClass("active");
             setTimeout(function () {
                 hideAllDropdowns();
             });
        });
    }

    function setDropdownPosition(selector, options) {
        if (options.position === 'top'){
            var topPosition = -Math.abs(options.maxHeight + 3);
            selector.closest(".multiselect-native-select").find(".multiselect-container").css({ "top": topPosition })
        }
    }

    $.fn.customMultiselect = function (options) {
        this.multiselect(options);
        bindEvents(this, options);
        addCustomCheckbox(this);
        if (this.attr('multiple') === "multiple") {
            updateSelectedText(this, options);
        } else {
            singleSelectElementClick(this);
        }
        setDropdownPosition(this, options);
        return this;
    }

    $.fn.addFancyScrollbar = function (options) {
        var btnGroupElement = $(this).nextAll(".btn-group:first");
        var multiselectDropDown = btnGroupElement.find('.multiselect-container');
        btnGroupElement.after(multiselectDropDown);

        var selectedOptionsCount = $(this).closest(".multiselect-native-select").find(".multiselect-container li").length;
        var listHeight = 250;
        if (options.listMinHeight && options.listMaxHeight) {
            listHeight = (selectedOptionsCount > 4) ? options.listMaxHeight : +(options.listMinHeight * selectedOptionsCount) + +options.offset;
        }
        else {
            listHeight = (options.height) ? (options.height) : listHeight;
        }

        multiselectDropDown.slimScroll(
            {
                height: listHeight,
            }
        );

        $(this).nextAll(".slimScrollDiv:first").addClass("hide").css({
            "box-shadow": "0 2px 4px 0 rgba(0,0,0,0.16),0 2px 10px 0 rgba(0,0,0,0.12)"
        }).height(listHeight).width($(this).nextAll(".btn-group:first").width());

        return this;
    }

}(jQuery));
