# Models/ViewModels

ViewModels for the views live here (suffix `...ViewModel` or `...Vm`). **Never pass an entity
straight to a View** — map it to a ViewModel first. Put `[Required]`, `[StringLength]`,
`[Display]`, `[RegularExpression]`, and custom `ValidationAttribute`s on the ViewModel
(tier-1 validation). Empty for now; feature work fills it in.
