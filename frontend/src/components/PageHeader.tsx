export function PageHeader({ title, description }: { title: string; description?: string }) {
  return (
    <header className="page-head">
      <h1 className="page-head-title">{title}</h1>
      {description ? <p className="page-head-desc">{description}</p> : null}
    </header>
  );
}
