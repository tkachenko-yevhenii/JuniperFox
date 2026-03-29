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
