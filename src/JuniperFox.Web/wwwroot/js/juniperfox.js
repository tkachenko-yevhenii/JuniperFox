window.JuniperFox = {
  getLastOpenedListId: function () {
    return localStorage.getItem('juniperfox.lastOpenedListId');
  },
  setLastOpenedListId: function (id) {
    if (id) {
      localStorage.setItem('juniperfox.lastOpenedListId', id);
    } else {
      localStorage.removeItem('juniperfox.lastOpenedListId');
    }
  },
};

(function () {
  if (!window.visualViewport || !document.body) {
    return;
  }

  var KEYBOARD_CLASS = 'jf-keyboard-open';
  var KEYBOARD_MIN_DIFF = 120;
  var baselineViewportHeight = window.visualViewport.height;

  function isEditableElement(element) {
    if (!element) {
      return false;
    }

    var tag = element.tagName;
    if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') {
      return true;
    }

    return element.isContentEditable === true;
  }

  function shouldRefreshBaseline(isEditing, heightDiff, viewportOffsetTop) {
    return !isEditing && heightDiff < 48 && viewportOffsetTop < 8;
  }

  function updateKeyboardState() {
    var activeElement = document.activeElement;
    var isEditing = isEditableElement(activeElement);
    var viewport = window.visualViewport;
    var viewportHeight = viewport.height;
    var viewportOffsetTop = viewport.offsetTop || 0;

    if (viewportHeight > baselineViewportHeight) {
      baselineViewportHeight = viewportHeight;
    }

    var baselineDiff = baselineViewportHeight - viewportHeight;
    var isKeyboardOpenByHeight = baselineDiff > KEYBOARD_MIN_DIFF;
    var isKeyboardOpenByOffset = viewportOffsetTop > 28;
    var isKeyboardOpen = isEditing && (isKeyboardOpenByHeight || isKeyboardOpenByOffset);

    document.body.classList.toggle(KEYBOARD_CLASS, isKeyboardOpen);

    if (shouldRefreshBaseline(isEditing, baselineDiff, viewportOffsetTop)) {
      baselineViewportHeight = viewportHeight;
    }
  }

  window.visualViewport.addEventListener('resize', updateKeyboardState);
  window.addEventListener('focusin', updateKeyboardState);
  window.addEventListener('focusout', function () {
    window.setTimeout(updateKeyboardState, 0);
  });
  window.addEventListener('orientationchange', function () {
    window.setTimeout(updateKeyboardState, 100);
  });

  updateKeyboardState();
})();
