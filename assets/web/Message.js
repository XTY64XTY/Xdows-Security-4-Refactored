/**
 * 消息提示条
 * @param {object} params 
 * @param {number} params.duration 持续时间（毫秒），默认`3000`
 * @param {number} params.zIndex 起始定位层级，默认`1000`
 */
function useMessage(params = {}) {
  const doc = document;
  const cssModule = `__${Math.random().toString(36).slice(2, 7)}`;
  const className = {
    box: `msg-box${cssModule}`,
    hide: `hide${cssModule}`,
    text: `msg-text${cssModule}`,
    icon: `msg-icon${cssModule}`
  }
  const style = doc.createElement("style");
  style.textContent = `
  @import"color.css";
  .${className.box}, .${className.icon}, .${className.text} {
    padding: 0;
    margin: 0;
    box-sizing: border-box;
  }
  .${className.box} {
    position: fixed;
    top: 0;
    left: 50%;
    display: flex;
    padding: 12px 16px;
    border-radius: var(--SmallBorderRadius);
    border: 1px solid var(--Theme-Border-color);
    background-color: var(--Theme-Background-color2);
    box-shadow: var(--Message-Shadow);
    white-space: nowrap;
    animation: ${className.box}-move .4s;
    transition: .4s all;
    transform: translate3d(-50%, 0%, 0);
    opacity: 1;
    overflow: hidden;
  }
  .${className.box}::after {
    content: "";
    position: absolute;
    left: 0;
    top: 0;
    height: 100%;
    width: 4px;
  }
  @keyframes ${className.box}-move {
    0% {
      opacity: 0;
      transform: translate3d(-50%, -100%, 0);
    }
    100% {
      opacity: 1;
      transform: translate3d(-50%, 0%, 0);
    }
  }
  .${className.box}.${className.hide} {
    opacity: 0;
    /* transform: translate3d(-50%, -100%, 0); */
    transform: translate3d(-50%, -100%, 0) scale(0);
  }
  .${className.icon} {
    display: inline-block;
    width: 18px;
    height: 18px;
    border-radius: 50%;
    overflow: hidden;
    margin-right: 6px;
    position: relative;
  }
  .${className.text} {
    font-size: 14px;
    line-height: 18px;
    color: var(--Text-color);
  }
  .${className.icon}::after,
  .${className.icon}::before {
    position: absolute;
    content: "";
    background-color: var(--Theme-Background-color2);
  }
  .${className.box}.info .${className.icon} {
    background-color: #1890ff;
  }
  .${className.box}.success .${className.icon} {
    background-color: #52c41a;
  }
  .${className.box}.warning .${className.icon} {
    background-color: #faad14;
  }
  .${className.box}.error .${className.icon} {
    background-color: #ff4d4f;
  }
  .${className.box}.info .${className.icon}::after,
  .${className.box}.warning .${className.icon}::after {
    top: 15%;
    left: 50%;
    margin-left: -1px;
    width: 2px;
    height: 2px;
    border-radius: 50%;
  }
  .${className.box}.info .${className.icon}::before,
  .${className.box}.warning .${className.icon}::before {
    top: calc(15% + 4px);
    left: 50%;
    margin-left: -1px;
    width: 2px;
    height: 40%;
  }
  .${className.box}.error .${className.icon}::after, 
  .${className.box}.error .${className.icon}::before {
    top: 20%;
    left: 50%;
    width: 2px;
    height: 60%;
    margin-left: -1px;
    border-radius: 1px;
  }
  .${className.box}.error .${className.icon}::after {
    transform: rotate(-45deg);
  }
  .${className.box}.error .${className.icon}::before {
    transform: rotate(45deg);
  }
  .${className.box}.success .${className.icon}::after {
    box-sizing: content-box;
    background-color: transparent;
    border: 2px solid var(--Theme-Background-color2);
    border-left: 0;
    border-top: 0;
    height: 50%;
    left: 35%;
    top: 13%;
    transform: rotate(45deg);
    width: 20%;
    transform-origin: center;
  }
  `.replace(/(\n|\t|\s)*/ig, "$1").replace(/\n|\t|\s(\{|\}|\,|\:|\;)/ig, "$1").replace(/(\{|\}|\,|\:|\;)\s/ig, "$1");
  doc.head.appendChild(style);
  /** 一直累加的定位层级 */
  let zIndex = params.zIndex || 1000;
  /**
   * 消息队列
   * @type {Array<HTMLElement>}
   */
  const messageList = [];

  /**
   * 获取指定`item`的定位`top`
   * @param {HTMLElement=} el 
   */
  function getItemTop(el) {
    let top = 10;
    for (let i = 0; i < messageList.length; i++) {
      const item = messageList[i];
      if (el && el === item) {
        break;
      }
      top += item.clientHeight + 20;
    }
    return top;
  }

  /**
   * 删除指定列表项
   * @param {HTMLElement} el 
   */
  function removeItem(el) {
    for (let i = 0; i < messageList.length; i++) {
      const item = messageList[i];
      if (item === el) {
        messageList.splice(i, 1);
        break;
      }
    }
    el.classList.add(className.hide);
    messageList.forEach(function(item) {
      item.style.top = `${getItemTop(item)}px`;
    });
  }

  /**
   * 显示一条消息
   * @param {string} content 内容
   * @param {"info"|"success"|"warning"|"error"} type 消息类型
   * @param {number} duration 持续时间，优先级比默认值高
   */
  function show(content, type = "info", duration) {
    const el = doc.createElement("div");
    el.className = `${className.box} ${type}`;
    el.style.top = `${getItemTop()}px`;
    el.style.zIndex = zIndex;
    el.innerHTML = `
    <span class="${className.icon}"></span>
    <span class="${className.text}">${content}</span>
    `;
    zIndex++;
    messageList.push(el);
    doc.body.appendChild(el);
    // 添加动画监听事件
    function animationEnd() {
      el.removeEventListener("animationend", animationEnd);
      setTimeout(removeItem, duration || params.duration || 3000, el);
    }
    el.addEventListener("animationend", animationEnd);
    function transitionEnd() {
      if (getComputedStyle(el).opacity !== "0") return;
      el.removeEventListener("transitionend", transitionEnd);
      el.remove();
    }
    el.addEventListener("transitionend", transitionEnd);
  }
  
  return {
    show,
    /**
     * 普通描述提示
     * @param {string} msg 
     */
    info(msg) {
      show(msg, "info");
    },
    /**
     * 成功提示
     * @param {string} msg 
     */
    success(msg) {
      show(msg, "success");
    },
    /**
     * 警告提示
     * @param {string} msg 
     */
    warning(msg) {
      show(msg, "warning");
    },
    /**
     * 错误提示
     * @param {string} msg 
     */
    error(msg) {
      show(msg, "error");
    }
  }
}
