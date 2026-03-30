let nextSubId = 1;
const subIdToHandler = new Map();

export function subscribeToList(dotNetHelper, listIdStr) {
  const handler = (e) => {
    if (e.key !== "jf.list.sync" || !e.newValue) return;
    try {
      const p = JSON.parse(e.newValue);
      if (p.listId === listIdStr) {
        dotNetHelper.invokeMethodAsync("OnListSyncFromOtherTab");
      }
    } catch {
      /* ignore */
    }
  };
  const id = nextSubId++;
  subIdToHandler.set(id, handler);
  window.addEventListener("storage", handler);
  return id;
}

export function unsubscribeFromList(subId) {
  const handler = subIdToHandler.get(subId);
  if (!handler) return;
  window.removeEventListener("storage", handler);
  subIdToHandler.delete(subId);
}

export function publishListChanged(listIdStr) {
  localStorage.setItem(
    "jf.list.sync",
    JSON.stringify({ listId: listIdStr, t: Date.now() })
  );
}
