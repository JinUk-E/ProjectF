mergeInto(LibraryManager.library, {
  FortressMobileState: function (json) {
    window.dispatchEvent(new CustomEvent('fortress-mobile-state', { detail: JSON.parse(UTF8ToString(json)) }));
  }
});
