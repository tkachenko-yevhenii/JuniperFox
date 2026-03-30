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

  function updateKeyboardState() {
    var activeElement = document.activeElement;
    var isEditing = isEditableElement(activeElement);
    var viewportHeight = window.visualViewport.height;
    var heightDiff = window.innerHeight - viewportHeight;
    var isKeyboardOpen = isEditing && heightDiff > KEYBOARD_MIN_DIFF;

    document.body.classList.toggle(KEYBOARD_CLASS, isKeyboardOpen);
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
