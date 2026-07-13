// Inline Lottie animations via lottie-web (works with React 19).
import { useEffect, useRef } from "react";
import lottie from "lottie-web";
import success from "../assets/lottie/success.json";
import empty from "../assets/lottie/empty.json";
import analytics from "../assets/lottie/analytics.json";
import account from "../assets/lottie/account.json";

const MAP = { success, empty, analytics, account };

export default function Anim({ name, size = 160, loop = true, style }) {
  const ref = useRef(null);

  useEffect(() => {
    const data = MAP[name];
    if (!ref.current || !data) return;
    const inst = lottie.loadAnimation({
      container: ref.current,
      renderer: "svg",
      loop,
      autoplay: true,
      animationData: data,
    });
    return () => inst.destroy();
  }, [name, loop]);

  return <div ref={ref} style={{ width: size, height: size, margin: "0 auto", ...style }} />;
}
