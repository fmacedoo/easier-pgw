<!-- Pos.svelte -->
<script>
	import { onMount } from 'svelte';

	let products = [];
	let cart = [];

	onMount(async () => {
		const response = await fetch('http://localhost:1337/products');
		products = await response.json();
	});

	function addToCart(product) {
		cart = [...cart, { id: product.id, name: product.name, price: product.price }];
	}

	function calculateTotal() {
		return cart.reduce((total, item) => total + item.price, 0);
	}

	function checkout() {
		// Implement your checkout logic here
		console.log('Checkout clicked!');
	}
</script>

<div class="flex max-w-4xl mx-auto mt-8">
	<div class="w-3/4 bg-white p-8 shadow-md">
		{#each products as product (product.id)}
			<div class="mb-4">
				<span class="text-lg font-bold">{product.name}</span>
				<span class="text-lg font-bold ml-2">${product.price.toFixed(2)}</span>
				<button
					on:click={() => addToCart(product)}
					class="ml-4 bg-blue-500 text-white py-1 px-2 rounded">Add to Cart</button
				>
			</div>
		{/each}
	</div>
	<div class="w-1/4 bg-gray-200 p-8">
		<h2 class="text-xl font-bold mb-4">Shopping Cart</h2>
		<ul class="mb-4">
			{#each cart as item (item.id)}
				<li class="flex justify-between items-center mb-2">
					<span class="text-md">{item.name}</span>
					<span class="text-md">${item.price.toFixed(2)}</span>
				</li>
			{/each}
		</ul>
		<div class="flex justify-between items-center mb-4">
			<span class="text-lg font-bold">Total:</span>
			<span class="text-lg font-bold">${calculateTotal().toFixed(2)}</span>
		</div>
		<button on:click={checkout} class="w-full bg-green-500 text-white py-2 px-4 rounded"
			>Checkout</button
		>
	</div>
</div>

<style>
	/* Tailwind styles can be added here if needed */
</style>
